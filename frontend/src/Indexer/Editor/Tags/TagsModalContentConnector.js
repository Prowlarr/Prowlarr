import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import TagsModalContent from './TagsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { indexerIds }) => indexerIds,
    createAllIndexersSelector(),
    createTagsSelector(),
    (indexerIds, allIndexers, tagList) => {
      const indexers = _.intersectionWith(allIndexers, indexerIds, (s, id) => {
        return s.id === id;
      });

      const indexerTags = _.uniq(_.concat(..._.map(indexers, 'tags')));

      return {
        indexerTags,
        tagList
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onAction() {
      // Do something
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(TagsModalContent);
